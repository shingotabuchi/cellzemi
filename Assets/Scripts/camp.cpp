//C++ code to provide a framework for spatio-temporal simulation of cells and various chemical concentrations
//current setup is for cAMP wave simulation in Dictyostelium discoideum cell plates
//see "Modeling oscillations and waves of cAMP in Dictyostelium discoideum cells",
//by J. Halloy, J. Lauzeral, A. Goldbeter, Biophysical chemistry, 72 (1998) 9-19
//this code is concerned with section 4 of that paper.
//Antal Zuiderwijk, Jan 2015, antal6@gmail.com
#include "opencv2/core/core.hpp"
#include "opencv2/highgui/highgui.hpp"
#include "opencv2/imgproc/imgproc_c.h"
#include "opencv2/imgproc/imgproc.hpp"
#include <iostream>
#include <random>//C++11
#include <csignal>
#include <fstream>
#include <sstream>
using namespace std;
using namespace cv;
 
//forward declares:
class grid;
Mat generaterandomframe(Size reso);
void signalHandler(int signum);
void evolverho(grid* dst, grid* rho_t, grid* beta, grid* gamma, grid* k_e, grid* sigma);
void evolvebeta(grid* dst, grid* rho_t, grid* beta, grid* gamma, grid* k_e, grid* sigma);
void evolvegamma(grid* dst, grid* rho_t, grid* beta, grid* gamma, grid* k_e, grid* sigma);
void evolvedevtime(grid* dst, grid* develloptime);
float calck_e(float time);
float calcsigma(float time);
float** calclaplacian(grid* ingrid);
 
//simulation constants:
const float timestep = 1; //in seconds
const float endtime = 25000; //seconds. TODO: can quit before endtime / infinite endtime
const float voxelpitch = 0.0001; //in meters
const int xvoxels = 960;
const int yvoxels = 600; //recomendation: take xvoxels/yvoxels the same as xresoluton/yresolution, to prevent distortion in the movie
 
//physics constants:
float timedesync = 2; //the Delta in the exponential distribution of the time desync, in minutes
float h = 5; //extra to intracellular volume ratio
float k_i = 1.7/60; // 1/60 factors are all to turn inverse minutes to inverse seconds
float k_t = 0.9/60;
float q = 4000;
float c = 10;
float k_1 = 2.5*0.036 / 60; //2.5 factors as in Lauzeral et al, proc. natl. acad. sci. 94, 1997, P9153
float invk_1 = 2.5*0.36/60;
float k_2 = 2.5*0.666 / 60;
float invk_2 = 2.5*0.00333/60;
float L_1 = 10;
float L_2 = 0.005;
float alpha = 3;
float mu = 1;
float eta = 0.3;
float labda = 0.01;
float theta = 0.01;
float epsilon = 1;
float D_gamma = 1e-4*1.5e-4 / 60; //conversion from cm^2/min to m^2/s. Diffusion co-efficient
//float D_gamma = 0; //for diffusion-free simulating
 
//movie output parameters:
const string outfilename = "movie.avi";
const float timescalefactor = 100; //how many seconds of simulation go in to one second of movie
const float frames_per_second = 24.0;
const float framepitch = 1/frames_per_second; //time between each frame
const Size resolution(960, 600);
bool showmovie = true;
bool savemovie = true;
VideoWriter* writer = NULL;
ofstream* outstream = NULL;
 
//class to hold one grid of one space-dependant variable. Hard-coded for 2D
class grid{
public: //all public all day erryday
    int xvoxels, yvoxels;
    float** array;
    grid(int xvoxels_in, int yvoxels_in){
        xvoxels = xvoxels_in; yvoxels = yvoxels_in;
        array = new float*[xvoxels];
        for(int ii = 0; ii < xvoxels; ii++)
            array[ii] = new float[yvoxels];
    }
    ~grid(){
        for(int ii = 0; ii < xvoxels; ii++)
            delete[] array[ii];
        delete[] array;
    }
    Mat genframe(Size reso, float time){
        Mat temp(yvoxels, xvoxels, CV_32FC1, Scalar(0,0,1.0));
        Mat dst, dstint;
        for(int ii = 0; ii < yvoxels; ii++){
            for(int jj = 0; jj < xvoxels; jj++){
                temp.at<float>(ii,jj) = array[jj][ii]; //add factors to get images properly normalized
            }
        }
        Scalar themean = mean(temp);
        //print stuff:
        //cout<<time<<" : "<<themean.val[0]<<endl;
        *outstream<<time<<" : "<<themean.val[0]<<endl;
        //scale to make the movie nicer:
        temp *= 80;
        resize(temp, dst, reso, 0, 0, CV_INTER_AREA);
        dst.convertTo(dstint, CV_8UC1);
        return dstint;
    }
};
 
int main(int argc, char* argv[]){
    signal(SIGINT, signalHandler); //allows the movie thus far to be salvaged after SIGINT
    //some openCV objects to faciliate displaying results:
    writer = new VideoWriter(outfilename, CV_FOURCC('M','J','P','G'), frames_per_second, resolution);
    outstream = new ofstream("outfile.txt");
    namedWindow("cAMP Simulation");
    Mat videoframe, temp;
    char kar; //used to receive waitkey
    string outfilebase = "capture";
    string outfilename;
    string extension = ".png";
    stringstream stream;
    default_random_engine generator;
    exponential_distribution<float> dist(1.0/timedesync); //for setting initial conditions
 
    //grids holding the information at start of an iteration:
    grid* rho_t = new grid(xvoxels, yvoxels);
    grid* beta = new grid(xvoxels, yvoxels);
    grid* gamma = new grid(xvoxels, yvoxels);
    grid* develloptime = new grid(xvoxels, yvoxels);
    //below 2 are making a [redacted] amount of hyperbolic tangent calls. Any alternative?
    grid* k_e = new grid(xvoxels, yvoxels); //recalculated from develloptime each iteration
    grid* sigma = new grid(xvoxels, yvoxels); //recalculated from develloptime each iteration
 
    //grids with the _new suffix will hold the state after an interation
    grid* rho_t_new = new grid(xvoxels, yvoxels);
    grid* beta_new = new grid(xvoxels, yvoxels);
    grid* gamma_new = new grid(xvoxels, yvoxels);
    grid* develloptime_new = new grid(xvoxels, yvoxels);
 
    //set some initial conditions:
    for(int ii = 0; ii < xvoxels; ii++){
        for(int jj = 0; jj < yvoxels; jj++){
            rho_t->array[ii][jj] = 0.0;
            beta->array[ii][jj] = 0.0;
            gamma->array[ii][jj] = 0.0; //uniform initial cAMP concentration
            develloptime->array[ii][jj] = dist(generator)*60; //*60 for seconds
        }
    }
 
    float time = 0;
    float nextframetime = 0;
    while(time < endtime){
        //cout<<time<<" "<<nextframetime<<endl;
        //calculate current k_e and sigma:
        for(int ii = 0; ii < xvoxels; ii++){
            for(int jj = 0; jj < yvoxels; jj++){
                k_e->array[ii][jj] = calck_e(develloptime->array[ii][jj]);
                sigma->array[ii][jj] = calcsigma(develloptime->array[ii][jj]);
            }
        }
        //calculate new situation based on current:
        evolverho(rho_t_new, rho_t, beta, gamma, k_e, sigma);
        evolvebeta(beta_new, rho_t, beta, gamma, k_e, sigma);
        evolvegamma(gamma_new, rho_t, beta, gamma, k_e, sigma);
        evolvedevtime(develloptime_new, develloptime);
        //replace rho_t, beta, and gamma with their updated states:
        //TODO: so many allocs and frees... could be better...
        delete rho_t; delete beta; delete gamma; delete develloptime;//get rid of the old situation
        rho_t = rho_t_new; //the old situation is now the new situation
        beta = beta_new;
        gamma = gamma_new;
        develloptime = develloptime_new;
        rho_t_new = new grid(xvoxels, yvoxels); //create a new slate for the next new situation
        beta_new = new grid(xvoxels, yvoxels);
        gamma_new = new grid(xvoxels, yvoxels);
        develloptime_new = new grid(xvoxels, yvoxels);
 
        //check whether to write a frame:
        if(time >= nextframetime){
            temp = gamma->genframe(resolution, time);
            cvtColor(temp, videoframe, CV_GRAY2BGR, 3);
            if(savemovie)
                writer->write(videoframe);
            if(showmovie){
                imshow("cAMP Simulation", videoframe);
                kar = waitKey(1);
                if(kar == 'q'){
                    writer->release();
                    exit(0);
                }
                if(kar == 'c'){
                    stream.str("");
                    stream<<outfilebase<<time<<extension;
                    outfilename = stream.str();
                    imwrite(outfilename, videoframe);
                }
            }
            nextframetime += framepitch*timescalefactor;
        }
        //finally advance one timestep:
        time += timestep;
        cout<<time<<endl;
    }
    writer->release();
    return 0;
}
 
//generates a frame filled with random noise. Used for testing videowriter and resize
Mat generaterandomframe(Size reso){
    Mat temp(reso, CV_8UC3);
    for(int ii = 0; ii < reso.height; ii++){
        for(int jj = 0; jj < reso.width; jj++){
            temp.at<Vec3b>(ii,jj)[0] = rand() % 255;
            temp.at<Vec3b>(ii,jj)[1] = rand() % 255;
            temp.at<Vec3b>(ii,jj)[2] = rand() % 255;
        }
    }
    return temp;
}
 
void evolverho(grid* dst, grid* rho_t, grid* beta, grid* gamma, grid* k_e, grid* sigma){
    float f_1, f_2, thisgamma, thisrho;
    for(int ii = 0; ii < dst->xvoxels; ii++){
        for(int jj = 0; jj < dst->yvoxels; jj++){
            thisgamma = gamma->array[ii][jj];
            thisrho = rho_t->array[ii][jj];
            f_1 = (k_1 + k_2*thisgamma) / (1+thisgamma);
            f_2 = (k_1*L_1 + k_2*L_2*c*thisgamma) / (1+c*thisgamma);
            dst->array[ii][jj] = rho_t->array[ii][jj] + timestep*(-f_1*thisrho + f_2*(1-thisrho));
        }
    }
}
 
void evolvebeta(grid* dst, grid* rho_t, grid* beta, grid* gamma, grid* k_e, grid* sigma){
    float phi, thissigma, thisrho, thisgamma, thisbeta, Y;
    for(int ii = 0; ii < dst->xvoxels; ii++){
        for(int jj = 0; jj < dst->yvoxels; jj++){
            thissigma = sigma->array[ii][jj];
            thisrho = rho_t->array[ii][jj];
            thisgamma = gamma->array[ii][jj];
            thisbeta = beta->array[ii][jj];
            Y = thisrho*thisgamma / (1+thisgamma);
            phi = (alpha*(labda*theta + epsilon*Y*Y))/(1+alpha*theta+epsilon*Y*Y*(1+alpha));
            dst->array[ii][jj] = beta->array[ii][jj] + timestep*(q*thissigma*phi - (k_i + k_t)*thisbeta);
        }
    }
}
 
void evolvegamma(grid* dst, grid* rho_t, grid* beta, grid* gamma, grid* k_e, grid* sigma){
    float thisbeta, thisgamma, thisk_e, thislaplacian;
    float** laplacian = calclaplacian(gamma);
    //cout<<laplacian[250][250]<<endl;
    for(int ii = 0; ii < dst->xvoxels; ii++){
        for(int jj = 0; jj < dst->yvoxels; jj++){
            thisbeta = beta->array[ii][jj];
            thisgamma = gamma->array[ii][jj];
            thisk_e = k_e->array[ii][jj];
            thislaplacian = laplacian[ii][jj];
            dst->array[ii][jj] = gamma->array[ii][jj] + timestep * (k_t*thisbeta/h - thisk_e*thisgamma + D_gamma*thislaplacian);
        }
        delete[] laplacian[ii];
    }
    delete[] laplacian;
}
 
void evolvedevtime(grid* dst, grid* develloptime){
    for(int ii = 0; ii < dst->xvoxels; ii++){
        for(int jj = 0; jj < dst->yvoxels; jj++){
            dst->array[ii][jj] = develloptime->array[ii][jj] + timestep;
        }
    }
}
 
void signalHandler(int signum){
    cout<<"Received interupt signal. Wrapping up movie and closing."<<endl;
    if(writer != NULL)
        writer->release();
    if(outstream != NULL)
        outstream->close();
    exit(signum);
}
 
float calck_e(float time){
    //return (5.4/60); //use to test time-only model
    float time_in_mins = time/60;
    float k_e_in_invmins = 6.5 + 3*tanh((time_in_mins-260)/30);
    return k_e_in_invmins / 60; //to return in inverse seconds
}
 
float calcsigma(float time){
    //return (0.6/60); //use to test time-only model
    float time_in_mins = time/60;
    float sigma_in_invmins = 0.3 + 0.25 * tanh((time_in_mins-200)/50);
    return sigma_in_invmins / 60; //to return in inverse seconds
}
 
float** calclaplacian(grid* ingrid){
    float** laplacian = new float*[xvoxels];
    float up, down, left, right, thisvalue;
    for(int ii = 0; ii < xvoxels; ii++){
        laplacian[ii] = new float[yvoxels];
        for(int jj = 0; jj <yvoxels; jj++){
            thisvalue = ingrid->array[ii][jj];
            if(ii == 0)
                left = thisvalue;
            else
                left = ingrid->array[ii-1][jj];
            if(ii == xvoxels-1)
                right = thisvalue;
            else
                right = ingrid->array[ii+1][jj];
            if(jj == 0)
                up = thisvalue;
            else
                up = ingrid->array[ii][jj-1];
            if(jj == yvoxels-1)
                down = thisvalue;
            else
                down = ingrid->array[ii][jj+1];
            laplacian[ii][jj] = (left+right+up+down - 4*thisvalue) / (voxelpitch*voxelpitch);
        }
    }
    return laplacian;
}